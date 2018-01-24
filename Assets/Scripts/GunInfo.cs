using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class GunInfo : MonoBehaviour {

	public GunObject m_gun;

	public Text m_name;
	public Text m_description;

	public Image m_artwork;

	public Text m_damage;
	public Text m_fireRate;
	public Text m_accuracy;
	public Text m_bulletCount;
	public Text m_reloadTime;

	public Text m_baseCost;

	private void Start () 
	{
		SetupItem ();
	}

	private void OnValidate()
	{
		SetupItem ();
	}

	public void SetupItem()
	{
		m_name.text = m_gun.m_name;
		m_description.text = m_gun.m_description;
		m_artwork.sprite = m_gun.m_artwork;
		m_damage.text = m_gun.m_gunLevelData[0].m_damage.ToString();
		m_fireRate.text = m_gun.m_gunLevelData[0].m_fireRate.ToString();
		m_accuracy.text = m_gun.m_gunLevelData[0].m_accuracy.ToString();
		m_bulletCount.text = m_gun.m_gunLevelData[0].m_bulletCount.ToString();
		m_reloadTime.text = m_gun.m_gunLevelData[0].m_reloadTime.ToString();
		m_baseCost.text = "$" + m_gun.m_gunLevelData[0].m_cost.ToString ();
	}
}
